<template>
<nav class="navbar navbar-expand-sm bg-dark navbar-dark static-top">
    <router-link to="/" class="navbar-brand">
        <img src="http://placehold.it/150x50?text=GIS+Photos" alt="Logo" class="mybrand" />
    </router-link>
    <button class="navbar-toggler" type="button" data-toggle="collapse" data-target="#navbarResponsive" aria-controls="navbarResponsive" aria-expanded="false" aria-label="Toggle navigation">
        <span class="navbar-toggler-icon"></span>
    </button>
    <div class="collapse navbar-collapse my-navs" id="navbarResponsive">
        <ul class="navbar-nav">
            <li class="nav-item">
                <router-link to="/" class="nav-link">Home</router-link>
            </li>
            <li class="nav-item">
                <router-link to="/About" class="nav-link">About</router-link>
            </li>
            <li class="nav-item">
                <router-link to="/MapRenderer" class="nav-link">Map</router-link>
            </li>
            <li class="nav-item">
                <router-link to="/PhotoUpload" class="nav-link">Share Photo</router-link>
            </li>
        </ul>

        <ul class="navbar-nav ml-auto my-right-navs">
            <template v-if="$store.getters.isLoggedIn">
                <li class="nav-item">
                    <span class="nav-link text-light">{{ $store.state.user?.email }}</span>
                </li>
                <li class="nav-item">
                    <a href="#" class="nav-link" @click.prevent="logout">Logout</a>
                </li>
            </template>
            <template v-else>
                <li class="nav-item">
                    <router-link to="/SignUp" class="nav-link">Sign Up</router-link>
                </li>
                <li class="nav-item">
                    <router-link to="/Login" class="nav-link">Login</router-link>
                </li>
            </template>
        </ul>
    </div>
</nav>
</template>

<script>
export default {
    name: 'NavBar',
    methods: {
        logout() {
            this.$store.dispatch('logout');
            this.$router.push('/');
        },
    },
}
</script>

<!-- Add "scoped" attribute to limit CSS to this component only -->

<style lang="scss" scoped>
#nav {
    padding: 0 20px 0 20px;

    a {
        font-weight: bold;
        color: #ffffff;

        background-color: #42b983;

        &.router-link-exact-active {
            color: #42b983;
        }
    }
}

.mybrand {
    margin-left: 130%;
    border-radius: 6px;
    height: 30px;
}

.my-navs {
    margin-left: 10%;
}

.my-right-navs {
    padding-right: 9%;
}

.my-navs a {
    color: #ffffff;
}

ul .nav-item .nav-link i {
    color: #ffffff;
}

@media only screen and (max-width: 1115px) {
    .mybrand {
        display: block;
        margin-left: 100%;
    }

    .my-navs {
        margin-left: 13%;
    }

    .my-right-navs {
        margin-right: 6%;
        color: red;
    }
}

@media only screen and (max-width: 998px) {
    .mybrand {
        display: block;
        margin-left: 40%;
    }

    .my-navs {
        margin-left: 6%;
    }

    .my-right-navs {
        margin-right: 0%;
    }
}

@media only screen and (max-width: 871px) {
    .mybrand {
        display: block;

        margin-left: 100%;
    }

    .my-navs {
        margin-left: 13%;
    }

    .my-right-navs {
        margin-right: 2%;
        color: red;
    }
}

@media only screen and (max-width: 768px) {

    /* For desktop: */
    .mybrand {
        display: block;

        margin-left: 125%;
    }

    .my-navs {
        margin-left: 16%;
    }

    .my-right-navs {
        margin-right: 9%;
        color: red;
    }
}

@media only screen and (max-width: 688px) {

    /* For desktop: */
    .mybrand {
        display: block;

        margin-left: 100%;
    }

    .my-navs {
        margin-left: 16%;
    }

    .my-right-navs {
        margin-right: 7%;
    }
}

@media only screen and (max-width: 604px) {
    .mybrand {
        display: block;
        margin-left: 44%;
    }

    .my-navs {
        margin-left: 7%;
    }

    .my-right-navs {
        margin: 0%;
    }
}
</style>
